# Windows desktop shell

The desktop application is a native WPF host around the local ASP.NET Core operator console. It starts the local web service on a loopback-only dynamic port and embeds it with WebView2.

The shell intentionally runs unelevated by default. Privileged operations are delegated to explicit execution providers and later will be brokered through narrowly scoped elevation rather than running the whole UI as Administrator.

Release packaging will place the Web host beside the desktop executable so the shell can start the complete local stack from one installation.
