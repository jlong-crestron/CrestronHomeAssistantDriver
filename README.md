# CrestronHomeAssistantDriver
POC of Crestron Home Assistant SDK. 
Currently only websocket transports are functional with a limited command set (Media Players)

# Getting started
 - Create access token from HA Lovelace and include in the `Constants` file under the DefaultAccessToken.
 - Change the local address transport in `Constants` to respect the address of your HA instance.

# Using
 - Build solution with your HA instance transport and token,
 - in bin/DEBUG or bin/RELEASE you should see your atrifacts which will include `CrestronHomeAssistantDriver.dll` 
 - reference this  DLL in your project.

