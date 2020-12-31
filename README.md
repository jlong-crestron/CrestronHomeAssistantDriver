# CrestronHomeAssistantDriver
POC of Crestron Home Assistant SDK. 
Currently only websocket transports are functional with a limited command set (Media Players)

# Getting started
 - Create access token from HA Lovelace and include in your `configuration.json` file under the `accessToken`.
 - Change the local address transport in `Constants` to respect the address of your HA instance.

  # Configuration
  HomeAssistant properties are defined within a `configuration` file using JSON schema. below is an example to use in your project

  ```
  {
    "host": "<ADDRESS OF YOUR HA INSTANCE>",
    "port": 8123,
    "path": "/api/websocket",
    "accessToken": "<YOUR ACCESS TOKEN>",
    "secure": false,
    "debug": true
  }
  ```
# Using
 - Build solution with your HA instance transport and token,
 - in bin/DEBUG or bin/RELEASE you should see your atrifacts which will include `CrestronHomeAssistantDriver.dll` 
 - reference this  DLL in your project.
    ```
    <Reference Include="CrestronHomeAssistantDriver">
      <HintPath>..\..\..\CrestronHomeAssistantDriver\CrestronHomeAssistantDriver\bin\Debug\CrestronHomeAssistantDriver.dll</HintPath>
    </Reference>
    ```
 - Currently Media Players are lightly supported. We are working on a rework to how interfacing with devices work. Here is an example of controlling a Roku through Home Assistant.
  ```
    new ServiceCommandObject()
    {
        Domain = "remote",
        Service = "send_command",
        Data = new RemoteServiceData.SendCommand()
        {
            Command = RemoteServiceData.Commands.Select,
            EntityId = <Your Roku entityId>
        }
    };
  ```

