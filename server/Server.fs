module Snakes.App

open System

open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open GameHub

// ---------------------------------
// Views
// ---------------------------------
module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title [] [ encodedText "snakes" ]
            ]
            body [] content
        ]

    let index games =
        [ h1 [] [
              encodedText "Snakes Server 0.1"
          ]
          h3 [] [ encodedText "Games:" ]
          ul
              []
              (games
               |> Seq.map (fun game -> li [] [ encodedText game ])
               |> List.ofSeq) ]
        |> layout

// ---------------------------------
// Web app
// ---------------------------------
let indexHandler _ =
    let view = Views.index State.Games.Keys
    htmlView view

let webApp: HttpHandler =
    choose [ GET
             >=> choose [ route "/api" >=> warbler indexHandler ]
             setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------
let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")

    clearResponse
    >=> setStatusCode 500
    >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (_: CorsPolicyBuilder) = ()

let configureApp (app: IApplicationBuilder) =
    let env =
        app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.IsDevelopment() with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler(errorHandler))
        //            .UseHttpsRedirection()) -- TODO: consider enabling this, not most likely needed when hosted on cluster
        .UseCors(
            configureCors
        )
        .UseStaticFiles()
        .UseRouting()
        .UseEndpoints(fun endpoints -> endpoints.MapHub<GameHub>("/gamehub") |> ignore)
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddSignalR() |> ignore

    services.AddHostedService<BackgroundServices.GameService>()
    |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot =
        Directory.GetCurrentDirectory()

    let webRoot =
        Path.Combine(contentRoot, "WebRoot")

    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseContentRoot(contentRoot)
                .UseWebRoot(webRoot)
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0
