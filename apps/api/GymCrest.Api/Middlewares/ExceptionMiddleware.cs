namespace GymCrest.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                bool expected = ClientErrorMessage.IsExpected(ex);
                if (!expected)
                {
                    _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                }

                int statusCode = expected ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
                context.Response.StatusCode = statusCode;
                var response = new
                {
                    StatusCode = statusCode,
                    Message = ClientErrorMessage.For(ex),
                    Success = false
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
