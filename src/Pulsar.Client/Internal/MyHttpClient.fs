namespace Pulsar.Client.Internal

open Pulsar.Client.Common
open Pulsar.Client.Api
open Flurl.Http


type internal MyHttpClient(config: PulsarClientConfiguration) = 
    let tokenString = config.Authentication.GetAuthData().GetCommandData()
    let httpClient = (new FlurlClient(config.ServiceAddresses.Head.AbsoluteUri))
                        .WithHeader("Authorization", "Bearer " + tokenString)

    member this.GetAsync(path: string) = 
        httpClient.Request(path).GetStringAsync()

    member this.GetHttpClient() = httpClient

                        
                         