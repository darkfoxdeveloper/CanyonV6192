# Canyon

This project is a fork from [__Comet__](https://gitlab.com/spirited/comet) and all rights over Comet are reserved by Gareth Jensen "Spirited".

The project is split between three servers: an account server, game server and ai server. The account server authenticates players, while the game server services players in the game world and the ai server handles ai over the NPCs in the world. This simple three-server architecture acts as a good introduction into server programming and concurrency patterns. The server is interoperable with the Conquer Online game client, but a modified client will not be provided.

This still a work in progress and is not recommended to starters. No support will be given to creating events, NPCs or anything like that. But if you want to work with Canyon you may report bugs and we will keep the main repository updated with bug fixes to whoever wants to try it.

When the live server leaves the Beta Stage, we will start keeping stable versions of Canyon in the [__main__](https://gitlab.com/world-conquer-online/canyon/canyon/-/tree/main) main repository, if you download from [__development__](https://gitlab.com/world-conquer-online/canyon/canyon/-/tree/development) make sure you know what you are doing and that you are ready to face bugs.

| Patch | Pipeline Status | Quality Gate | Description |
| ----- | --------------- | ------------ | ----------- |
| [__development__](https://gitlab.com/world-conquer-online/canyon/canyon/-/tree/development) | [![pipeline status](https://gitlab.com/world-conquer-online/canyon/canyon/badges/development/pipeline.svg)](https://gitlab.com/world-conquer-online/canyon/canyon/-/commits/development) | [![Quality Gate Status](https://sonarqube.ftwmasters.com.br/api/project_badges/measure?project=f8fe2c3a-6ab8-4842-93a9-05119f155e8c&metric=alert_status&token=squ_47b1b46df7c1ba81ebae052d4ef90e7334d59058)](https://sonarqube.ftwmasters.com.br/dashboard?id=f8fe2c3a-6ab8-4842-93a9-05119f155e8c) | Targets the official 6090 client. |
| [__main__](https://gitlab.com/world-conquer-online/canyon/canyon/-/tree/main) | [![pipeline status](https://gitlab.com/world-conquer-online/canyon/canyon/badges/main/pipeline.svg)](https://gitlab.com/world-conquer-online/canyon/canyon/-/commits/main) | Not published | Targets the official 6090 client. |

## Getting Started

Before setting up the project, download and install the following:

* [.NET 7](https://dotnet.microsoft.com/download) - Primary language compiler
* [MariaDB](https://mariadb.org/) - Recommended flavor of MySQL for project databases 
* [MySQL Workbench](https://dev.mysql.com/downloads/workbench/) - Recommended SQL editor and database importer
* [Visual Studio Code](https://code.visualstudio.com/) - Recommended editor for modifying and building project files

In a terminal, run the following commands to build the project (or build with Shift+Ctrl+B):

```
dotnet restore
dotnet build
```

This project has been tested already with Debian and will work well in other environments.

## Common Questions & Answers

### Why my Login Server won't start claiming to have a "invalid_client" error?

Canyon make use of a API to authenticate users, the APIs that the game uses will not be provided and you have two options: 

* Build your own API and feed the login server with the required information
* Change the code in Login Server to connect to a MySQL database instead

### Why can't I connect to the server?

There are a few reasons why you might not be able to connect. First, check that you can connect locally using a loopback adapter. If you can connect locally, but cannot connect externally, then check your firewall settings and port forwarding settings. If you can connect to the Account server but not the Game server, then check your IP address and port in the `realm` table. Confirm that your firewall allows the port, and that port forwarding is also set up for the Game server (and not just the Account server).

## Legality

Algorithms and packet structuring used by this project for interoperability with the Conquer Online game client is a result of reverse engineering. By Sec. 103(f) of the DMCA (17 U.S.C. § 1201 (f)), legal possession of the Conquer Online client is permitted for this purpose, including circumvention of client protection necessary for archiving interoperability (though the client will not be provided for this purpose). Comet is a non-profit, academic project and not associated with TQ Digital Entertainment. All rights over Comet are reserved by Gareth Jensen "Spirited". All rights over the game client are reserved by TQ Digital Entertainment.
