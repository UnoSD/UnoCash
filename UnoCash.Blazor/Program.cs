using BlazorStrap;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnoCash.Blazor
{
    public class Program
    {
        public static Task Main(string[] args) => 
            CreateHostBuilder(args).Build()
                                   .RunAsync();

        public static WebAssemblyHostBuilder CreateHostBuilder(string[] args)
        {
            var wab = WebAssemblyHostBuilder.CreateDefault(args);
            
            wab.Services
               .AddBootstrapCss()
               .AddSingleton(new HttpClient());

            wab.RootComponents.Add<App>("app");

            return wab;
        }
    }
}
