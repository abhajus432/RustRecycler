Shared recyclers for teams: players on the same team can use one recycler at the same time.
Other teams are blocked: players from a different team can’t open a recycler that’s in use, and get a message explaining why.
Players without a team are blocked: they can’t open a recycler while a team is using it.
Configurable user limit: MaxUsers sets how many teammates can share one recycler at once.
Configurable messages: both messages can be edited in oxide/config/RustRecycler.json, and {0} in the limit message is replaced with MaxUsers.
Chat and toast notifications: blocked players get the message both in chat and as a red on-screen toast.
Automatic cleanup: players are removed from a recycler when they close it, disconnect, leave their team or get kicked from it.
Credits: built with help from Claude, used through dnSpy for exploring Rust’s game code, looking up methods and hooks, and small C# syntax help.
