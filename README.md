# Rate Limiting with `System.Threading.RateLimiting`

This repository contains a two-sided demo of:

1. How to use `System.Threading.RateLimiting` to limit API response processing
2. How to use Polly retry policies to automatically delay execution based on server-sent delay

This is useful whenever interacting with APIs that actively provide feedback on rate limiting.

The .NET client side is implemented using [Polly](https://www.pollydocs.org/) while the JS client side is implemented using [Cockatiel](https://github.com/connor4312/cockatiel), which is a port of Polly to TypeScript.

## Running

To run the demo:

```shell
cd service
dotnet run        # Starts our simulated service that returns a 429

# In another console
cd client
dotnet run        # Starts our invocation of the service API

# For the JS side
cd client-js
pnpm i
pnpm dev
```

You should see that some client requests are rejected with a 429 and the client re-executes the request after the delay.
