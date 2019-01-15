# Discord Music Player
The Discord Music Player is small Windows console application that can play your local music library in a Discord voice channel using your own Discord bot. 

## How to use
Download and extract the binary or compile the application from source.


### Configuration
The application loads the `config.xml` on startup. There is a template file named `config.sample.xml` you can rename to 


### Commands
Commands can be entered in the console application window or they can be send as private messages to the bot user via Discord. The user must be whitelisted by the `AllowedUsers` array in the configuration file.
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