namespace ArtemisBank.WebApi.Extensions
{
    public static class AppExtension
    {
        public static void UseSwaggerExtension(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Artemis Banking Pro API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Artemis Banking Pro API";
            });
        }
    }
}
