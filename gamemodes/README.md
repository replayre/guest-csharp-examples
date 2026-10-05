# guest-csharp-examples-gamemodes
An example bundle that implements a complex FFA, racing and free roam server

## Nuget dependencies

### Installing nuget dependencies
```sh
dotnet restore --verbosity normal
```

### Updating dependencies
Dependencies are pinned with a floating version
To update to a more recent build use the following commands

```sh
dotnet restore --force-evaluate
```

## Building
- `dotnet build` or `dotnet build -c Release`

## Deploying
We recommend symlinking your dist/ folder into the data/ugc/ folder
of your server

`ln -s /opt/replay/guest-csharp-examples/gamemodes/dist /opt/replay/server/data/ugc/gamemodes`

You can then start the bundle via

`start gamemodes`