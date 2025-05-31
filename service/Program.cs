using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Configure the rate limiting behavior here.
builder.Services.AddRateLimiter(_ =>
{
  _.AddSlidingWindowLimiter(
    policyName: "4_concurrent_sliding",
    options =>
    {
      options.PermitLimit = 4;
      options.Window = TimeSpan.FromSeconds(10);
      options.SegmentsPerWindow = 5;
      options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
      options.QueueLimit = 2;
      options.AutoReplenishment = true;
    }
  );

  // The default status code is 503; we want to send a 429 instead.
  _.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

  // Send a retry after with a random time.
  _.OnRejected = async (context, cancel) =>
  {
    var retryAfter = TimeSpan.FromSeconds(Random.Shared.Next(4, 10));

    context.HttpContext.Response.Headers.Append(
      HeaderNames.RetryAfter,
      ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo)
    );

    Console.WriteLine(
      $"Sending {HeaderNames.RetryAfter} with value {retryAfter.TotalSeconds}"
    );

    await ValueTask.CompletedTask;
  };
});

var app = builder.Build();

app.UseRateLimiter();

var requests = 0;

// An HTTP GET endpoint which simulates some long running work that's 2.5 to 6s.
app.MapGet(
    "/{job}",
    async (int job, HttpContext context) =>
    {
      var workload = Random.Shared.Next(2_500, 6_000);

      await Task.Delay(workload);

      context.Response.Headers.Append("x-job", job.ToString());

      Interlocked.Increment(ref requests);

      Console.WriteLine(
        $"Executed workload {requests} for job {job} with time: {workload}ms"
      );

      return $"Job {job} completed in: {workload}";
    }
  )
  .RequireRateLimiting("4_concurrent_sliding"); // Attach our rate limiting policy

app.Run();
