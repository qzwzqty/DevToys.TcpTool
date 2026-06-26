# TCP Tool DevToys Extension Design

Date: 2026-06-25

## Goal

Create a DevToys GUI extension from scratch for local debugging. The extension provides one TCP tool with a tabbed interface for TCP Client and TCP Server workflows.

## Scope

First version includes:

- DevToys .NET 8 class library extension.
- One `IGuiTool` registered in DevToys.
- Top tab switch between Client and Server.
- TCP client connect, disconnect, send, receive, and log.
- TCP server listen, stop, multi-client list, selected-client send, receive, and log.
- HEX-first payload workflow.
- Simple HEX conversion previews to binary and text.
- Local debugging launch settings.

First version excludes:

- CLI commands.
- Production packaging metadata beyond what local debugging needs.
- TCP message framing, sticky-packet handling, or protocol-specific parsing.
- TLS, UDP, proxy, serial port, or file transfer.

## Architecture

Use a small layered design:

- `TcpToolGui`: DevToys `IGuiTool` implementation and UI composition.
- `TcpClientSession`: owns one outbound TCP client connection.
- `TcpServerHost`: owns the TCP listener and connected client sessions.
- `TcpServerClient`: represents one accepted TCP client.
- `HexPayloadConverter`: validates and converts HEX payloads.
- `LogEntry`: immutable log model for connection, send, receive, and error events.

The UI layer does not directly manipulate sockets. It validates user intent, calls the session/host services, and appends service events to visible logs.

## UI Design

The tool has one page with top tabs:

### Client Tab

Fields and actions:

- Host input.
- Port input.
- Connect button.
- Disconnect button.
- HEX payload input.
- Send button.
- Receive/log output.
- Conversion preview for selected or current HEX payload: HEX, binary, text.

Behavior:

- Connect opens a TCP connection to the specified host and port.
- Disconnect closes the connection.
- Send converts HEX text to bytes and writes bytes to the connection.
- Received bytes are appended as log entries and displayed as uppercase HEX by default.

### Server Tab

Fields and actions:

- Listen IP input.
- Port input.
- Start button.
- Stop button.
- Connected client list.
- Selected-client HEX payload input.
- Send button.
- Receive/log output.

Behavior:

- Start begins listening on the specified IP and port.
- Stop stops listening and disconnects all clients.
- Accepted clients appear in the client list.
- Selecting a client enables sending HEX bytes to that client.
- Received bytes are logged with client endpoint information.

## Data Flow

### Client send

User enters HEX -> UI calls `HexPayloadConverter.Parse` -> UI calls `TcpClientSession.SendAsync` -> session writes to stream -> log records sent bytes.

### Client receive

`TcpClientSession` read loop receives bytes -> raises event/callback -> UI appends log entry -> log view renders uppercase HEX and preview text.

### Server receive

`TcpServerHost` accepts client -> creates `TcpServerClient` -> client read loop receives bytes -> host raises event/callback with endpoint -> UI appends log entry.

### Server send

User selects client and enters HEX -> UI parses bytes -> UI calls selected `TcpServerClient.SendAsync` through host -> log records sent bytes.

## HEX Conversion Rules

- Accept whitespace between bytes.
- Accept optional `0x` prefixes.
- Normalize output to uppercase two-character byte groups separated by spaces.
- Reject odd-length HEX digit sequences.
- Reject non-HEX characters except whitespace and valid `0x` prefixes.
- Binary preview renders each byte as eight bits.
- Text preview uses UTF-8 first version behavior.

## Error Handling

- Invalid host, IP, port, connection state, or HEX input is reported in the visible log.
- Connection failures are caught and logged.
- Background read-loop exceptions are caught and logged.
- Disconnect and Stop are idempotent from the UI perspective.
- Stopping the server disconnects all connected clients.
- No sticky-packet or split-packet correction is attempted; each socket read is logged as one received byte block.

## Testing

Automated tests should cover:

- `HexPayloadConverter` valid parsing.
- `HexPayloadConverter` invalid input rejection.
- HEX normalization.
- Binary preview conversion.
- UTF-8 text preview conversion.

Manual or integration checks should cover:

- Client connects to server on loopback.
- Client sends HEX and server logs bytes.
- Server sends HEX and client logs bytes.
- Server stop disconnects clients.

Verification command:

- `dotnet build`

## Implementation Notes

Create the project from scratch in the current directory. Use the DevToys extension development pattern documented by DevToys: a .NET class library referencing `DevToys.Api`, exporting an `IGuiTool`, and using `launchSettings.json` with `EXTRAPLUGIN=$(TargetDir)` for local debugging.
