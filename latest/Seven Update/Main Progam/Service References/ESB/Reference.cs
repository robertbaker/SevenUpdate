﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.20506.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SevenUpdate.ESB {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ESB.IEventSystem", CallbackContract=typeof(SevenUpdate.ESB.IEventSystemCallback), SessionMode=System.ServiceModel.SessionMode.Required)]
    public interface IEventSystem {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IEventSystem/Subscribe")]
        void Subscribe();
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IEventSystem/Unsubscribe")]
        void Unsubscribe();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IEventSystemCallback {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IEventSystem/OnDownloadDone")]
        void OnDownloadDone(bool errorOccurred);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IEventSystem/OnInstallDone")]
        void OnInstallDone(bool errorOccurred);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IEventSystem/OnErrorOccurred")]
        void OnErrorOccurred(string errorDescription);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IEventSystem/OnDownloadProgressChanged")]
        void OnDownloadProgressChanged(ulong bytesTransferred, ulong bytesTotal);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IEventSystem/OnInstallProgressChanged")]
        void OnInstallProgressChanged(string updateTitle, int progress, int updatesComplete, int totalUpdates);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IEventSystemChannel : SevenUpdate.ESB.IEventSystem, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class EventSystemClient : System.ServiceModel.DuplexClientBase<SevenUpdate.ESB.IEventSystem>, SevenUpdate.ESB.IEventSystem {
        
        public EventSystemClient(System.ServiceModel.InstanceContext callbackInstance) : 
                base(callbackInstance) {
        }
        
        public EventSystemClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
                base(callbackInstance, endpointConfigurationName) {
        }
        
        public EventSystemClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public EventSystemClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public EventSystemClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, binding, remoteAddress) {
        }
        
        public void Subscribe() {
            base.Channel.Subscribe();
        }
        
        public void Unsubscribe() {
            base.Channel.Unsubscribe();
        }
    }
}
