## Overview
RustRecycler lets teammates share a recycler. Players on the same team can use one recycler at the same time, while players from other teams (or without a team) are blocked until it's free.

## Features
- **Shared recyclers for teams**: teammates can use the same recycler together.
- **Other teams are blocked**: they get a message explaining why.
- **Players without a team are blocked** while a team is using the recycler.
- **Configurable user limit**: set how many teammates can share one recycler.
- **Configurable messages**, shown in chat and as an on-screen toast.
- **Automatic cleanup**: players are removed when they close the recycler, disconnect, leave their team or get kicked from it.
  
## Configuration
```json
{
  "MaxUsers": 1,
  "Message": "Hey! The recycler is in use by a different team.",
  "Message2": "Sorry, the recycler only allow's: {0} teammates per Recycler, from the same team!"
}
```
- **MaxUsers**: how many teammates can use one recycler at the same time.
- **Message**: shown when another team is using the recycler.
- **Message2**: shown when the recycler is full. `{0}` is replaced with `MaxUsers`.

After editing the config, reload the plugin with `oxide.reload RustRecycler`.

## Credits
Built with help from Claude Code, used through dnSpy for exploring Rust's game code, looking up methods and hooks, and C# syntax.
