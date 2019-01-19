# Discord Music Player
The Discord Music Player is small Windows console application that can play your local music library in a Discord voice channel using your own Discord bot. 

## How to use
Download and extract the binary or compile the application from source. You can start the bot by launching the `DiscordMusicPlayer.exe`. **But first you need to create a configuration file.**


#### Configuration
You need to create a `config.xml` file in the same directory as the application. There is a template file named `config.sample.xml` you can use as a quick start. 

The configuration file is a xml file. The root element is called `Config`. 

| Element name | Type                                | Description                                                                                                                          | Example                                                                                             |
|--------------|-------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| Token        | string                              | The Discord account token.                                                                                                           | `<Token>AbCdEfGh1337.T1HsI5n0tAre8lT0ken</Token>`                                               |
| TokenType    | enum _(User, Bearer, Bot, Webhook)_ | The type of your Discord account. You should always use _Bot_.                                                                       | `<TokenType>Bot</TokenType>`                                                                      |
| Guild        | string                              | The name or id of the guild.                                                                                                         | `<Guild>My super cool server</Guild>`                                                            |
| Channel      | string                              | The name or id of the audio channel.                                                                                                 | `<Channel>Music Channel</Channel>`                                                               |
| Shuffle      | boolean _(true, false)_             | If this is set to _true_ the playlist will be shuffled. The default is _false_.                                                      | `<Shuffle>true</Shuffle>`                                                                         |
| Autoplay     | boolean _(true, false)_             | If this is set to _true_ the application will start the playback immediately after joining the audio channel. The default is _true_. | `<Autoplay>true</Autoplay>`                                                                       |
| Volume       | integer _(0 to 100)_                | Defines the playback volume in percentage. The default is _100_.                                                                     | `<Volume>100</Volume>`                                                                            |
| Directories  | array of string                     | Defines the directories of you local music library. You can add multiple directories too.                                            | `<Directories>`<br/>`  <Directory>C:\Users\Username\Music\</Directory>`<br/>`</Directories>` |
| AllowedUsers | array of string                     | Defines the Discord user accounts that can control the bot via private messages.                                                     | `<AllowedUsers>`<br/>`  <AllowedUser>Admin#xxxx</AllowedUser>`<br/>`</AllowedUsers>`          |

The elements `Token`, `TokenType`, `Guild`, `Channel` and `Directories` are required to start the application!

#### Commands
You can use command to control the bot while running. Commands can be entered in the console application window or they can be send as private messages to the bot user via Discord. The user must be whitelisted by the `AllowedUsers` array in the configuration file.
```
play                    - Resumes the playback of the current track.
play <title>            - Plays the track with the given title.
stop                    - Stops the playback.
next                    - Skips the current track and plays the next title on the playlist.
volume                  - Gets the current volume.
volume <volume>         - Sets the volume (0 - 100).
join <channel>          - Joins the audio channel on the current guild.
join <guild> <channel>  - Joins the audio channel on the given guild.
help                    - Shows this useful message.
info                    - Shows the version number and links to the creators homepage.
exit                    - Closes the application. This can only be used in console mode!
```


## FAQ - Frequently Asked Questions

##### Why doesn't the bot play any music? I can't hear it.
There are two reasons for that. If you're using the bot for the first time you need to make sure that the requirements were installed on your computer.

If you used the bot a few minutes ago restarted it and it is muted now you have to close the bot by closing the console window or use the `exit` command.
When you kill the process in any other way the bot cannot disconnect from the discord server properly. It will timeout in a minute but if you rejoin the server while the bot user is sill in the voice channel it is not able to play any music. Just restart the bot and try again. 

##### Can i remote control the bot?
Yes you can. Simply add you Discord user name to the `AllowedUsers` list in the configuration file. You need to restart the bot after editing the file. Now you can send commands via the private Discord chat.

##### Can i play music from a NAS or any other network storage?
Yes. You can use any mounted drive like `X:\MyMusic\` and any Windows file sharing path like `\\192.168.178.100\MyMusic`.

##### Can i play YouTube music videos with this?
No. Sorry this feature is not supported.

## Requirements
You need to install these requirements in order to run the bot:

* [Microsoft Visual C++ 2015 Redistributable (x86)](https://www.microsoft.com/de-de/download/details.aspx?id=48145) *(vc_redist.x86.exe)*

## License
This project is licensed under the terms of the [MIT license](LICENSE).


Thanks to the [Discord.Net](https://github.com/RogueException/Discord.Net) for creating such great api!