using System.Text.Json;
using Polly;
using Polly.Retry;

using var client = new HttpClient();

// Construct the pipeline
var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
  .AddRetry(
    new RetryStrategyOptions<HttpResponseMessage>
    {
      // The number of retry attempts
      MaxRetryAttempts = 4,
      // The predicate that determines whether the result should be handled by the pipeline
      ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        .HandleResult(r =>
          r.StatusCode == System.Net.HttpStatusCode.TooManyRequests
        )
        .Handle<HttpRequestException>(),
      // A delay generation strategy that reads the Retry-After header.
      DelayGenerator = static args =>
      {
        if (
          args.Outcome.Result is HttpResponseMessage responseMessage
          && TryGetDelay(responseMessage, out TimeSpan delay)
        )
        {
          Console.WriteLine($"⮑  Setting retry delay to: {delay}");

          return new ValueTask<TimeSpan?>(delay);
        }

        // Returning null means the retry strategy will use its internal delay for this attempt.
        return new ValueTask<TimeSpan?>((TimeSpan?)null);

        static bool TryGetDelay(
          HttpResponseMessage response,
          out TimeSpan delay
        )
        {
          if (response.Headers.TryGetValues("Retry-After", out var values))
          {
            delay = TimeSpan.FromSeconds(
              int.Parse(values.FirstOrDefault() ?? "0")
            );

            return true;
          }

          delay = TimeSpan.FromSeconds(0);

          return false;
        }
      }
    }
  )
  .Build();

var completed = 0;

// Execute 10 tasks to observe the behavior of the retries.
var tasks = Enumerable
  .Range(0, 10)
  .Select(async i =>
  {
    await pipeline.ExecuteAsync(
      async (state, token) =>
      {
        Console.WriteLine($"Sending request: {state.i}");

        var response = await state.client.GetAsync(
          $"http://localhost:5020/{state.i}"
        );

        var header = JsonSerializer.Serialize(
          response.Headers.ToDictionary(h => h.Key, h => h.Value)
        );

        var prefix = response.IsSuccessStatusCode ? "✅" : "⛔️";

        if (response.IsSuccessStatusCode)
        {
          Interlocked.Increment(ref completed);
        }

        Console.WriteLine(
          $"{prefix} Code: {response.StatusCode}, Headers: {header}"
        );

        return response;
      },
      (client, i, completed)
    );
  });

await Task.WhenAll([.. tasks]);

Console.WriteLine($"Completed: {completed}");
