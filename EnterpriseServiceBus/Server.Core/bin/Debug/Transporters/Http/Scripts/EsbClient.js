/*!
 * Enterprise Service Bus Javascript Library v1.0
 * http://geodecisions.com
 *
 * Copyright Geodecisions, Inc. All rights reserved. 
 */

(function(root, factory) {
    if (typeof define === 'function' && define.amd) {
        define(['jQuery'], factory);
    } else {
        root.esb = factory(root.jQuery);
    }
}(this, function($) {
    'use strict';

    function generateUUID() {
        var d = new Date().getTime();
        var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (d + Math.random() * 16) % 16 | 0;
            d = Math.floor(d / 16);
            return (c == 'x' ? r : (r & 0x7 | 0x8)).toString(16);
        });
        return uuid;
    }

    // ctor
    var client = function(host, port, busOptions, options) {
        // in case user didn't use new keyword
        if (!(this instanceof client)) {
            return new client(host, port, busOptions, callType, options);
        }

        var ajaxDefaults = {
            dataType: 'jsonp'
        };

        var busDefaults = {
            clientId: generateUUID(),
            name: 'Default JsClient',
            description: 'Default JsClient',
            onReady: function () { },
            mode:'async'
        };

        this.subQState = 0;
        this.subQ = [];
        this.pubQ = [];
        
        this.callbacks = [];
        this.clientId = null;
        this.serverConfig = null;
        this.scheme = 'http';
        this.host = host;
        this.port = port;

        this.baseUrl = this.scheme + '://' + this.host + ':' + this.port + '/esb/api';

        this.ajaxOptions = $.extend(ajaxDefaults, options);
        this.ajaxOptions = $.extend(this.ajaxOptions, { context: this });
        this.busOptions = $.extend(busDefaults, busOptions);

        this.clientId = this.busOptions.clientId + '.JSCLIENT';

        this.clientConnected = false;
        
        // lets load the config from the server
        var configUrl = this.scheme + '://' + this.host + ':' + this.port + '/esb/services/configuration/config';

        $.ajax(configUrl, this.ajaxOptions).done(function(data) {
            //console.log(data);
            this.serverConfig = data;

            // setup http
            var httpSettings = $.grep(this.serverConfig.transporterSettings, function(setting) {
                return (setting.name === 'HttpTransporter');
            })[0];

            this.busBaseUrl = this.scheme + '://' + httpSettings.host + ':' + httpSettings.port + '/esbdyn';

            //console.log(this.busBaseUrl);
            
            this.connection = $.hubConnection(this.busBaseUrl, { useDefaultPath: false });
            this.esbHub = this.connection.createHubProxy('esbHub');

            var thisContext = this;

            // gets called from the esb server
            this.esbHub.on('pushMessage', function(message) {
                console.log('pushMessage');
                console.log(message);
                var filteredCbs = $.grep(thisContext.callbacks, function(cb) {
                    if (cb.uri === message.uri) {
                        if (cb.options && cb.options.ignoreLocal) {
                            return cb.clientId != message.clientId;
                        }
                        return true;
                    }
                    return false;
                });

             
                jQuery.each(filteredCbs, function(idx, cb) {

                    var hydrated = null;

                    try {
                        hydrated = $.parseJSON(message.payload);
                    } catch(e) {
                        // log?
                    }

                    cb.callback(hydrated, message);
                });
            });

            // gets called from the esb server
            this.esbHub.on('forceClose', function(message) {
                this.esbHub.stop();
            });

            console.log('cors?:' + jQuery.support.cors);
            //jQuery.support.cors = false;
            //console.log('cors?:' + jQuery.support.cors);
            
            this.connection.start({ waitForPageLoad: false }).done(function () {
                console.log('esb conn ok');

                var now = new Date();
                var nowUtc = new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), now.getUTCHours(), now.getUTCMinutes(), now.getUTCSeconds());
              
                // we need to announce ourselves
                thisContext.esbHub.invoke('announce', { id: thisContext.clientId, name: thisContext.busOptions.name, description: thisContext.busOptions.description, lastPulse: nowUtc }).done(function () {
                    
                    thisContext.clientConnected = true;
                    
                    thisContext.onConnected.apply(thisContext);
                    thisContext.busOptions.onReady(thisContext);

                    jQuery.each(thisContext.subQ, function (idx, deferredSub) {
                        deferredSub.resolve();
                    });

                    // start heartbeat
                    var pulseFunc = function () {
                        var now = new Date();
                        var nowUtc = new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), now.getUTCHours(), now.getUTCMinutes(), now.getUTCSeconds());
                        thisContext.esbHub.invoke('pulse', thisContext.clientId, nowUtc);
                    };

                    pulseFunc(); // call once right away
                    thisContext.pulseId = setInterval(pulseFunc, 60 * 1000); //1 every minute
                    
                });
            });
        });

        // very ugly wait function for sync, if requested.
        var waitFunc = function (context) {
            console.log(context.clientConnected);
            if (context.clientConnected) {
                //ok
            } else {
                setTimeout(waitFunc, 500, context);
            }
        };

        if (this.busOptions.mode === 'sync') {
            //console.log('sync');
            waitFunc(this);
        }
        
        return this;
    };

    //events
    client.prototype.onConnected = function() {
    };
    
    client.prototype.onDisconnected = function () {
    };
    
    client.prototype.makeAjaxCall = function(url, options) {
        var extendedOptions = $.extend(this.ajaxOptions, options);
        return $.ajax(url, extendedOptions);
    };

    /**
    * Subscribe to a uri and provide a callback
    *
    *
    */
    client.prototype.Subscribe = function (subUri, callback, options) {
        var context = this;
        if (this.clientConnected) {
            var subMessage1 = { uri: subUri, machineName: window.location.host, clientId: this.clientId, callback: callback, options: options };
            this.callbacks.push(subMessage1);
            this.esbHub.invoke('subscribe', subMessage1);
        } else {
            var internalDef = $.Deferred();

            internalDef.done(function () {
                var subMessage2 = { uri: subUri, machineName: window.location.host, clientId: context.clientId, callback: callback, options: options };
                //console.log(subMessage2);
                context.callbacks.push(subMessage2);
                context.esbHub.invoke('subscribe', subMessage2).done(function () {
                    //if (context.subQState === 0) {
                        //console.log('def sub start');
                    //}
                    context.subQState++;
                    if (context.subQState === context.subQ.length) {
                        //console.log('def pub start');
                        // this is the last thing in the subQ
                        // call publishes
                        
                        context.subQ = []; // reset q

                        jQuery.each(context.pubQ, function (idx, deferredPub) {
                            deferredPub.resolve();
                        });

                        context.pubQ = []; // reset q
                    }
                });
            });

            this.subQ.push(internalDef);
        }
    };
    

    /**
    * UnSubscribe a uri and provide a callback
    *
    * @param {object} [optional] options jQuery ajax options to override defaults
    *
    */
    client.prototype.UnSubscribe = function(subUri, callback, options) {
        var context = this;

        var subMessage = { uri: subUri, machineName: machineName, clientId: this.clientId, callback: callback };
 
        var filteredCbs = $.grep(this.callbacks.callbacks, function(cb) {
            return (cb.uri === subMessage.uri && cb.machineName === subMessage.machineName && cb.clientId === subMessage.clientId);
        });

        jQuery.each(filteredCbs, function(idx, cb) {
            var loc = $.inArray(cb, context.callbacks);
            context.callbacks.splice(loc, 1);
        });

        this.esbHub.invoke('unsubscribe', subMessage);
    };


    /**
      * Publish to a uri and provide a callback
      *
      * @param {string} pubUri a uri in the format * / * / * (need 3 parts as of 6-27-02013)
      * @Param {string|object} message the json string message or object to send as a message
      * @param {object} [optional] options jQuery ajax options to override defaults
      * @param {number} [optional] options.minify defaults to true.  set to false to pretty print the message
      * @return {Promise} Returns jQuery ajax promise
      *
      */
    client.prototype.Publish = function (pubUri, message, options) {
        var context = this;
        
        if (this.clientConnected) {
            
            var bytes1 = [];

            var defaultMessage1 = (message instanceof String) ? message : (options && options.minify) ?
                JSON.stringify(message) :
                JSON.stringify(message, null, 4);

            for (var i = 0; i < defaultMessage1.length; ++i) {
                bytes1.push(defaultMessage1.charCodeAt(i));
            }
            
            var esbMessage1 = {
                id: generateUUID(),
                type: typeof msg,
                uri: pubUri,
                clientId: this.clientId,
                publisher: {  machineName: window.location.host },
                payload: bytes1
            };

            this.esbHub.invoke('publish', pubUri, esbMessage1);
        } else {
            var internalDef = $.Deferred();

            internalDef.done(function () {
                //console.log('deferredpub');
                var msg = message;
                var uri = pubUri;
                var opts = options;

                var bytes = [];
                
                var defaultMessage = (msg instanceof String) ? msg : (opts && opts.minify) ?
                    JSON.stringify(msg) :
                    JSON.stringify(msg, null, 4);
                
                for (var i = 0; i < defaultMessage.length; ++i) {
                    bytes.push(defaultMessage.charCodeAt(i));
                }
                
                var esbMessage2 = {
                    id: generateUUID(),
                    type: typeof msg,
                    uri: uri,
                    clientId: context.clientId,
                    publisher: { machineName: window.location.host },
                    payload: bytes
                };
                //console.log(esbMessage2);
                context.esbHub.invoke('publish', uri, esbMessage2);
                //console.log('deferredpub_end');
            });

            //var queuedPub = {deferred: internalDef, uri: subUri, message: message};
            
            this.pubQ.push(internalDef);
        }
    };

    /**
    * Shutdown the bus
    *
    *
    */
    client.prototype.Shutdown = function (options) {

        clearInterval(this.pulseId);
        
        var thisContext = this;
        
        this.esbHub.invoke('clientShutdown', this.clientId).done(function() {

            // clear all connections/objects
            thisContext.connection.stop();
            thisContext.callbacks = [];
            thisContext.serverConfig = null;
            thisContext.clientConnected = false;
        });
        
        //if (options && !options.async) {

        //    $.ajax({
        //        url: this.busBaseUrl + '/waitForShutdown',
        //        type: "POST",
        //        data: JSON.stringify({ clientId: this.clientId }),
        //        dataType: "json",
        //        contentType: "application/json; charset=utf-8",
        //        async: false,
        //        success: function () {
                    
        //        }
        //    });
        //}
    };
    



    return {
        Client: client
    };
    
}));