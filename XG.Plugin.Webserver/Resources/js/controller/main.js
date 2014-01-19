﻿//
//  main.js
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
//
//  Copyright (c) 2012 Lars Formella
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//

define(['./module'], function (ng) {
	'use strict';

	ng.controller('MainCtrl', ['$rootScope', '$scope', '$modal', 'ipCookie', 'SignalrFactory', 'SignalrTableFactory', 'VERSION',
		function ($rootScope, $scope, $modal, ipCookie, SignalrFactory, SignalrTableFactory, VERSION)
		{
			$scope.VERSION = VERSION;

			$scope.passwordOk = false;
			$modal.open({
				keyboard: false,
				backdrop: 'static',
				templateUrl: 'passwordDialog.html',
				controller: 'PasswordDialogCtrl'
			}).result.then(function (password)
			{
				$scope.passwordOk = true;
				$.connection.hub.start().done(
					function ()
					{
						$rootScope.$emit('OnConnected', password);
					}
				).fail(
					function (message)
					{
						$scope.openErrorDialog(message);
					}
				);
				$.connection.hub.error(
					function (message)
					{
						$scope.openErrorDialog(message);
					}
				);
				ipCookie('xg.password', password, { expires: 21, path: '/' });
			});

			$scope.openErrorDialog = function (message)
			{
				$modal.open({
					keyboard: false,
					backdrop: 'static',
					templateUrl: 'errorDialog.html',
					controller: 'ErrorDialogCtrl',
					resolve:
					{
						message: function ()
						{
							return message;
						}
					}
				});
			};

			$scope.openXdccDialog = function ()
			{
				$modal.open({
					keyboard: true,
					backdrop: true,
					templateUrl: 'xdccDialog.html',
					controller: 'XdccDialogCtrl'
				});
			};

			// build this here, because the dialogs will respawn and recreate stuff
			$scope.servers = [];
			$scope.serverSignalr = new SignalrTableFactory();
			$scope.serverSignalr.initialize('serverHub', $scope, 'servers', undefined, 'tableParamsServer');

			$scope.channelSignalr = new SignalrTableFactory();
			$scope.channelSignalr.initialize('channelHub', $scope, 'channels', undefined, 'tableParamsChannel');
			$scope.channels = [];

			$scope.openServerChannelsDialog = function ()
			{
				$modal.open({
					keyboard: true,
					backdrop: true,
					templateUrl: 'serverChannelDialog.html',
					controller: 'ServerChannelDialogCtrl',
					resolve:
					{
						serverSignalr: function ()
						{
							return $scope.serverSignalr;
						},
						channelSignalr: function ()
						{
							return $scope.channelSignalr;
						}
					}
				});
			};

			$scope.apiSignalr = new SignalrTableFactory();
			$scope.apiSignalr.initialize('apiHub', $scope, 'api', undefined, 'tableParamsApi');
			$scope.api = [];

			$scope.openApiDialog = function ()
			{
				$modal.open({
					keyboard: true,
					backdrop: true,
					templateUrl: 'apiDialog.html',
					controller: 'ApiDialogCtrl',
					resolve:
					{
						signalr: function ()
						{
							return $scope.apiSignalr;
						}
					}
				});
			};

			$scope.configSignalr = new SignalrFactory();
			$scope.configSignalr.initialize('configHub', $scope, 'config');
			$scope.config = [];

			$scope.openConfigDialog = function ()
			{
				$modal.open({
					keyboard: true,
					backdrop: true,
					templateUrl: 'configDialog.html',
					controller: 'ConfigDialogCtrl',
					resolve:
					{
						signalr: function ()
						{
							return $scope.configSignalr;
						}
					}
				});
			};

			$rootScope.settings = {
				showOfflineBots: ipCookie('xg.showOfflineBots'),
				humanDates: ipCookie('xg.humanDates'),
				showBotsInView: ipCookie('xg.showBotsInView')
			};

			if ($rootScope.settings.showOfflineBots == undefined)
			{
				$rootScope.settings.showOfflineBots = false;
			}
			if ($rootScope.settings.humanDates == undefined)
			{
				$rootScope.settings.humanDates = false;
			}
			if ($rootScope.settings.showBotsInView == undefined)
			{
				$rootScope.settings.showBotsInView = true;
			}

			$scope.flipSetting = function (setting)
			{
				var newValue = !$rootScope.settings[setting];
				$rootScope.settings[setting] = newValue;
				ipCookie('xg.' + setting, newValue ? '1' : '0', { expires: 21, path: '/' });
			};

			$scope.slide = 1;
			$scope.slideTo = function (slide)
			{
				$scope.slide = slide;
				$rootScope.$emit('OnSlideTo', slide);
			};
		}
	]);
});