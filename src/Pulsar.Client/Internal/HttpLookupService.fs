namespace Pulsar.Client.Internal

open Pulsar.Client.Api
open System.Net.Http
open Pulsar.Client.Common
open System.Text.Json
open System.Net
open System
open FSharp.Json
open Flurl.Http
open Newtonsoft.Json

type internal HttpLookupService (config: PulsarClientConfiguration) =

    let httpClient = new MyHttpClient(config)

    interface ILookupService with

        member this.GetPartitionsForTopic (topicName: TopicName) =
            backgroundTask {
                let! metadata = (this :> ILookupService).GetPartitionedTopicMetadata topicName.CompleteTopicName
                if metadata.Partitions > 0 then
                    return Array.init metadata.Partitions topicName.GetPartition
                else
                    return [| topicName |]
            }
    
        member this.GetPartitionedTopicMetadata (topicName: CompleteTopicName) = 
            backgroundTask {
                let lookupName = topicName.ToString().Replace("://", "/")
                let path = sprintf "admin/v2/%s/partitions" lookupName
                let! response = httpClient.GetHttpClient().Request(path).GetStringAsync()
                let metadata = JsonConvert.DeserializeObject<PartitionedTopicMetaData>(response)
                return {Partitions = metadata.partitions}
            }

        member this.GetBroker(topicName: CompleteTopicName) = 
            backgroundTask {
                let path = "lookup/v2/topic/" + topicName.ToString().Replace("://", "/")
                let! response = httpClient.GetHttpClient().Request(path).GetStringAsync()
                let lookupResult = JsonConvert.DeserializeObject<TopicLookupResult>(response)

                let uri = Uri(lookupResult.brokerUrl)
                let resultEndpoint = DnsEndPoint(uri.Host, uri.Port)
                return { LogicalAddress = LogicalAddress resultEndpoint; PhysicalAddress = PhysicalAddress resultEndpoint }
            }

        member this.GetTopicsUnderNamespace (ns : NamespaceName, isPersistent : bool) =
            backgroundTask {
                let path =  if isPersistent
                            then sprintf "admin/v2/namespaces/%s/topics?mode=0" ns.LocalName
                            else sprintf "admin/v2/namespaces/%s/topics?mode=1" ns.LocalName
                let! response = httpClient.GetHttpClient().Request(path).GetStringAsync()
                return JsonConvert.DeserializeObject<string seq>(response)
            }

        member this.GetSchema(topicName: CompleteTopicName, ?schemaVersion: SchemaVersion) =
            backgroundTask {
                let schemaName = topicName.ToString().Split([|"://"|], System.StringSplitOptions.None)[1]
                let path = sprintf "admin/v2/schemas/%s/schema" schemaName
                let path =
                    if schemaVersion.IsSome
                    then path + "/" + BitConverter.ToInt64(schemaVersion.Value.Bytes, 0).ToString()
                    else path
            
                let! response = httpClient.GetHttpClient().Request(path).GetStringAsync()
                
                let getSchemaResponse = JsonConvert.DeserializeObject<GetSchemaHTTPResponse>(response)

                if getSchemaResponse.``type``.Equals("STRING")
                then
                    let schemaInfo = { Name = "String"; Schema = [||]; Type = SchemaType.STRING; Properties = getSchemaResponse.properties}
                    let schemaVersion: SchemaVersion = {Bytes = BitConverter.GetBytes getSchemaResponse.version}
                    let topicSchema = {SchemaInfo = schemaInfo; SchemaVersion = Some(schemaVersion)};
                    return Some(topicSchema)
                else
                    return Option.None
            }