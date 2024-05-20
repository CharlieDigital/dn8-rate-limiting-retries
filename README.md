# Rate Limiting with `System.Threading.RateLimiting`

This repository contains a two-sided demo of:

1. How to use `System.Threading.RateLimiting` to limit API response processing
2. How to use Polly retry policies to automatically delay execution based on server-sent delay

This is useful whenever interacting with APIs that actively provide feedback on rate limiting.

## Running

To run the demo:

```shell
cd service
dotnet run        # Starts our simulated service that returns a 429

# In another console
cd client
dotnet run        # Starts our invocation of the service API
```

You should see that some client requests are rejected with a 429 and the client re-executes the request after the delay.
