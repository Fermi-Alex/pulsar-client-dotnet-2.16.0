namespace Pulsar.Client.Common

open System.Collections.Generic
open Pulsar.Client.Common

type internal PartitionedTopicMetaData(partitions: int) = 
    class
    let mutable partitionsInternal = partitions

    member this.partitions  
            with get() = partitionsInternal
            and set(value) = partitionsInternal <- value
    end

type internal TopicLookupResult(brokerUrl: string, brokerUrlTls: string,
        httpUrl: string, httpUrlTls: string, nativeUrl: string) = 
    class 
    let mutable brokerUrlInternal = brokerUrl
    let mutable brokerUrlTlsInternal = brokerUrlTls
    let mutable httpUrlInternal = httpUrl
    let mutable httpUrlTlsInternal = httpUrlTls
    let mutable nativeUrlInternal = nativeUrl

    member this.brokerUrl 
            with get() = brokerUrlInternal
            and set(value) = brokerUrlInternal <- value

    member this.brokerUrlTls
            with get()  = brokerUrlTlsInternal 
            and set(value) = brokerUrlTlsInternal <- value

    member this.httpUrl 
            with get() = httpUrlInternal
            and set(value) = httpUrlInternal <- value

    member this.httpUrlTls
            with get()  = httpUrlTlsInternal 
            and set(value) = httpUrlTlsInternal <- value

    member this.nativeUrl 
            with get() = nativeUrlInternal
            and set(value) = nativeUrlInternal <- value

    end