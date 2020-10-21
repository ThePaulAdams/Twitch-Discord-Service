# Twitch Discord Service
TDSBot / Twitchat / Twitcord / TwitchDiscordService, all of theses names may appear in the code somewhere.

This is a .Net Core 2 service to integrate A twich bot with a discord bot to allow basic echo'ing from one to the other.



## Installation

Remove the "Template" from the file name of "configTemplate.Json"

Insert the correct details into the fields.

You should have a MySQL Database running somewhere with a table that has the columns 
DB Name - DiscordTwitch

```sql
TABLE NAME - Channels
twitchId	varchar(255)	utf8mb4_0900_ai_ci
name		varchar(255)	utf8mb4_0900_ai_ci
type		varchar(255)	utf8mb4_0900_ai_ci
bio	        varchar(1024)	utf8mb4_0900_ai_ci
logo		varchar(255)	utf8mb4_0900_ai_ci
AuthToken	varchar(255)	utf8mb4_0900_ai_ci


TABLE NAME - Integrations
TwitchChannel		varchar(255)	utf8mb4_0900_ai_ci
DiscordGuild		varchar(255)	utf8mb4_0900_ai_ci
DiscordChannel		varchar(255)	utf8mb4_0900_ai_ci
IntegrationManager	varchar(255)	utf8mb4_0900_ai_ci

TABLE NAME - Quotes 
person	varchar(50)		utf8mb4_0900_ai_ci
quote	varchar(255)	utf8mb4_0900_ai_ci
date	date			
addedby	varchar(50)		utf8mb4_0900_ai_ci

```
Build and run.



## Usage
Twitch Commands
```python
#This will remove the bot from the current twitch channel
!remove

```
Discord Commands 
```python

#Message from a Twitch channel will be echoed into the Discord channel from where you send this command
!twitchadd "TwitchChannel"           :  !twitchadd Paul1337noob                        #Add the bot to a twitch channel

#This will remove the bot from the channel and stop echoing the chat to Discord
!twitchremchannel "TwitchChannel"    :  !twitchremchannel Paul1337noob                 #remove the bot from a twitch channel

#The bot MUST be in a channel to send a message
!twitch "Channel" "Message To Send"  :  !twitch Paul1337noob wow what an amazing bot   #Send a message to a twitch channel the bot is in

```

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)
