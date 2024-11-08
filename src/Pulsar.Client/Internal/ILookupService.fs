namespace Pulsar.Client.Internal

open Pulsar.Client.Common
open System.Threading.Tasks

type internal ILookupService =
    abstract member GetPartitionsForTopic: topic: TopicName -> Task<TopicName array>
    abstract member GetPartitionedTopicMetadata: topic: CompleteTopicName -> Task<PartitionedTopicMetadata>
    abstract member GetBroker: topic: CompleteTopicName -> Task<Broker>
    abstract member GetTopicsUnderNamespace: ns : NamespaceName * isPersistent : bool -> Task<string seq>
    abstract member GetSchema: topicName: CompleteTopicName * ?schemaVersion: SchemaVersion
                        -> Task<TopicSchema option>