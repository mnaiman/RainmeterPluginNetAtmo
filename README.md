# README #

This is plugin for accessing NetAtmo weather station from Rainmeter.

### License ###

 * Copyright (C) 2016 Michal Naiman
 *
 * This Source Code Form is subject to the terms of the GNU General Public
 * License; either version 2 of the License, or (at your option) any later
 * version. If a copy of the GPL was not distributed with this file, You can
 * obtain one at <https://www.gnu.org/licenses/gpl-2.0.html>

### How do I get set up? ###

* In RainMeter config use:

    [measure]
    Measure=Plugin
    Plugin=SystemVersion.dll
    ClientID=""
    ClientSecret=""
    Username=""
    Password=""
    DeviceModuleID=xx:xx:xx:xx:xx:xx
    Action=GetValue
    ValueName=Temperature/CO2/Humidity/Noise/Pressure

* In NetAtmo developer account, create your application and you get:
ClientID=""
    ClientSecret=""
    Username=""
    Password=""