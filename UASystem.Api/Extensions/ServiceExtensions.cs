using UASystem.Api.Application.IServices;
using UASystem.Api.Application.Services;
using UASystem.Api.Application.Services.PersonServices;
using UASystem.Api.Application.Services.PersonServices.PersonSubServices;
using UASystem.Api.Domain.Repositories;
using UASystem.Api.Infrastructure.Repositories;

namespace UASystem.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRegistrationServices(this IServiceCollection services)
        {
            services.AddScoped<ICountryRepository, CountryRepository>();
            services.AddScoped<ICountryService, CountryService>();
            services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
            services.AddScoped<IPersonRepository, PersonRepository>();
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ICreatePersonService, CreatePersonService>();


            //services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            //services.AddScoped<IGovernmentalInfoRepository, GovernmentalInfoRepository>();
            return services;
        }
    }
}
