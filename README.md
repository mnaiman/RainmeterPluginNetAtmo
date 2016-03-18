# README #

This is plugin for accessing [Netatmo weather station] station from [Rainmeter].

### How do I get set up? ###

* These are your login details to NetAtmo account:
```sh
    Username=""
    Password=""
```

* In [NetAtmo developer account] create your application and you get:
```sh
    ClientID=""
    ClientSecret=""
```
* In RainMeter config use:

```sh
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
```

* DeviceModuleID can be found in Netatmo account, or better - plugin will dump all found Devices to About log in Rainmeter
```sh
    DeviceModuleID=xx:xx:xx:xx:xx:xx
```

### License ###

Copyright (C) 2016 Michal Naiman

This Source Code Form is subject to the terms of the GNU General Public License; either version 2 of the License, or (at your option) any later version. You can obtain one at <https://www.gnu.org/licenses/gpl-2.0.html>

   [Netatmo weather station]: <https://www.netatmo.com/product/station>
   [Rainmeter]: <https://www.rainmeter.net/>
   [NetAtmo developer account]: <https://dev.netatmo.com/>