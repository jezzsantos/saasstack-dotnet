﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Infrastructure.Persistence.Azure {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Infrastructure.Persistence.Azure.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BlobName cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingBlobName {
            get {
                return ResourceManager.GetString("AnyStore_MissingBlobName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ContainerName cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingContainerName {
            get {
                return ResourceManager.GetString("AnyStore_MissingContainerName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ContentType cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingContentType {
            get {
                return ResourceManager.GetString("AnyStore_MissingContentType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to EntityId cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingEntityId {
            get {
                return ResourceManager.GetString("AnyStore_MissingEntityId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to EntityName cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingEntityName {
            get {
                return ResourceManager.GetString("AnyStore_MissingEntityName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Id cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingId {
            get {
                return ResourceManager.GetString("AnyStore_MissingId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Message cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingMessage {
            get {
                return ResourceManager.GetString("AnyStore_MissingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to QueueName cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingQueueName {
            get {
                return ResourceManager.GetString("AnyStore_MissingQueueName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Message cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingSentMessage {
            get {
                return ResourceManager.GetString("AnyStore_MissingSentMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SubscriptionName cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingSubscriptionName {
            get {
                return ResourceManager.GetString("AnyStore_MissingSubscriptionName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TopicName cannot be empty.
        /// </summary>
        internal static string AnyStore_MissingTopicName {
            get {
                return ResourceManager.GetString("AnyStore_MissingTopicName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The message is too large to send in a batch for an Azure Service Bus.
        /// </summary>
        internal static string AzureServiceBusStore_MessageTooLarge {
            get {
                return ResourceManager.GetString("AzureServiceBusStore_MessageTooLarge", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Azure Service Bus subscription name: &apos;{0}&apos; contains illegal characters or is not the correct length.
        /// </summary>
        internal static string ValidationExtensions_InvalidMessageBusSubscriptionName {
            get {
                return ResourceManager.GetString("ValidationExtensions_InvalidMessageBusSubscriptionName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Azure Service Bus topic name: &apos;{0}&apos; contains illegal characters or is not the correct length.
        /// </summary>
        internal static string ValidationExtensions_InvalidMessageBusTopicName {
            get {
                return ResourceManager.GetString("ValidationExtensions_InvalidMessageBusTopicName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Azure Storage Table/Blob/Queue name: &apos;{0}&apos; contains illegal characters or is not the correct length.
        /// </summary>
        internal static string ValidationExtensions_InvalidStorageAccountResourceName {
            get {
                return ResourceManager.GetString("ValidationExtensions_InvalidStorageAccountResourceName", resourceCulture);
            }
        }
    }
}
