# README #

This is plugin for accessing [Netatmo weather station] station from [Rainmeter].

### How do I get set up? ###

* In [NetAtmo developer account] create your application and you get:
```sh
    ClientID=""
    ClientSecret=""
```

* After first run, in About log, there will be authorization URL, open in browser
* After successful authorization redirect to not existing URL will be made
* Copy code from browser URL bar https://domain_not_exist_for_sure:56789/?state={random_number_must_be_same_as_about_log}&code={code}
```sh
    Code=""
```

* In RainMeter config use:

```sh
    [measure]
    Measure=Plugin
    Plugin=SystemVersion.dll
    ClientID=""
    ClientSecret=""
    Scope="read_station"
    Code=""
    DeviceModuleID=xx:xx:xx:xx:xx:xx
    Action=GetValue
    ValueName=Temperature/CO2/Humidity/Noise/Pressure
```

* DeviceModuleID can be found in Netatmo account, or better - plugin will dump all found Devices to About log in Rainmeter
```sh
    DeviceModuleID=xx:xx:xx:xx:xx:xx
```

### Prerequisites ###

* .NET Framework minimum 4.6.2 - https://dotnet.microsoft.com/download/dotnet-framework
* You need to install correct version of dll x64 for 64bit Rainmeter and x86 for 32bit

### License ###

Copyright (C) 2022 Michal Naiman

This Source Code Form is subject to the terms of the GNU General Public License; either version 2 of the License, or (at your option) any later version. You can obtain one at <https://www.gnu.org/licenses/gpl-2.0.html>

   [Netatmo weather station]: <https://www.netatmo.com/product/station>
   [Rainmeter]: <https://www.rainmeter.net/>
   [NetAtmo developer account]: <https://dev.netatmo.com/>
