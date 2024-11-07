namespace Pulsar.Client.Internal

open Pulsar.Client.Api
open System.Net.Http
open Pulsar.Client.Common
open System.Text.Json
open System.Net
open System

type internal HttpLookupService (config: PulsarClientConfiguration) =

    inherit BinaryLookupService(config, ConnectionPool(config))

    let httpClient = new HttpClient()
    do httpClient.BaseAddress <- config.ServiceAddresses.Head

    member this.GetPartitionsForTopic (topicName: TopicName) =
        backgroundTask {
            let! metadata = this.GetPartitionedTopicMetadata topicName.CompleteTopicName
            if metadata.Partitions > 0 then
                return Array.init metadata.Partitions topicName.GetPartition
            else
                return [| topicName |]
        }
    
    member this.GetPartitionedTopicMetadata (topicName: CompleteTopicName) = 
        backgroundTask {
            let lookupName = topicName.ToString().Replace("://", "/")
            let path = sprintf "admin/v2/%s/partitions" lookupName
            let! response = httpClient.GetAsync(path) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return JsonSerializer.Deserialize<PartitionedTopicMetadata>(content)
        }

    member this.GetBroker(topicName: CompleteTopicName) = 
        backgroundTask {
            let path = "lookup/v2/topic/" + topicName.ToString().Replace("://", "/")
            let! response = httpClient.GetAsync(path) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let lookupResult = JsonSerializer.Deserialize<LookupTopicResult>(content)
            let uri = Uri(lookupResult.BrokerServiceUrl)
            let resultEndpoint = DnsEndPoint(uri.Host, uri.Port)
            return { LogicalAddress = LogicalAddress resultEndpoint; PhysicalAddress = PhysicalAddress resultEndpoint }
        }
    member this.GetTopicsUnderNamespace (ns : NamespaceName, isPersistent : bool) =
        backgroundTask {
            let path =  if isPersistent
                        then sprintf "admin/v2/namespaces/%s/topics?mode=0" ns.LocalName
                        else sprintf "admin/v2/namespaces/%s/topics?mode=1" ns.LocalName
            let! response = httpClient.GetAsync(path) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let topics = JsonSerializer.Deserialize<string seq>(content)
            return PulsarResponseType.TopicsOfNamespace topics
        }

    member this.GetSchema(topicName: CompleteTopicName, ?schemaVersion: SchemaVersion) =
        backgroundTask {
            let schemaName = topicName.ToString().Split("://")[1]
            let path = sprintf "admin/v2/schemas/%s/schema" schemaName
            let path =
                if schemaVersion.IsSome
                then path + "/" + BitConverter.ToInt64(schemaVersion.Value.Bytes, 0).ToString()
                else path
            
            let! response = httpClient.GetAsync(path) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask

            if (String.IsNullOrEmpty content)
            then 
                return Option.None
            else 
                let getSchemaResponse = JsonSerializer.Deserialize<GetSchemaHTTPResponse>(content)
                if getSchemaResponse.Type.Equals("STRING")
                then
                    let schemaInfo = { Name = "String"; Schema = [||]; Type = SchemaType.STRING; Properties = getSchemaResponse.Properties}
                    let schemaVersion: SchemaVersion = {Bytes = BitConverter.GetBytes getSchemaResponse.Version}
                    let topicSchema = {SchemaInfo = schemaInfo; SchemaVersion = Some(schemaVersion)};
                    return Some(topicSchema)
                else
                    return Option.None
        }