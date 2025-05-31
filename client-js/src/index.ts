import {
  ConsecutiveBreaker,
  ExponentialBackoff,
  retry,
  handleAll,
  circuitBreaker,
  wrap,
  DelegateBackoff,
} from "cockatiel";

/**
 * Custom error type
 */
class RetryAfterError extends Error {
  constructor(public readonly after: number) {
    super();
  }
}

async function main() {
  console.log("starting...");

  const retryPolicy = retry(handleAll, {
    maxAttempts: 10,
    // backoff: new ExponentialBackoff({ maxDelay: 60000, initialDelay: 500 }),
    backoff: new DelegateBackoff((context) => {
      if (context.result instanceof RetryAfterError) {
        console.log(` ⮑  Retry after: ${context.result.after}`);
        return context.result.after * 1000;
      }

      return 1 * 1000 * context.attempt;
    }),
  });

  retryPolicy.onFailure((f) => console.log("Request failed:", f.duration));
  retryPolicy.onRetry((r) => console.log("Retry:", r.attempt));

  const circuitBreakerPolicy = circuitBreaker(handleAll, {
    halfOpenAfter: 10 * 1000,
    breaker: new ConsecutiveBreaker(3),
  });

  circuitBreakerPolicy.onStateChange((d) => {
    console.log("Breaker state change:", d);
  });

  const retryWithBreaker = wrap(retryPolicy, circuitBreakerPolicy);

  const requestFn = (i: number) =>
    retryWithBreaker.execute(async () => {
      console.log("Executing job:", i);
      const response = await fetch(`http://localhost:5020/${i}`);
      console.log(`Response ${response.status}:`, await response.text());

      if (response.status === 429) {
        const retryAfter = response.headers.get("retry-after");

        if (!retryAfter) {
          throw new Error(); // Generic error.
        }

        const delay = Number.parseInt(retryAfter);

        throw new RetryAfterError(delay);
      }
    });

  await Promise.all(Array.from({ length: 10 }, (_, i) => requestFn(i)));

  console.log("Done!");
}

await main();

export {};
