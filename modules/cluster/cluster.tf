variable "ssh_public_key_file" {
    type = string
    nullable = false
}

variable "principal_id" {
    type = string
    sensitive = true
    nullable = false
}

variable "principal_secret" {
    type = string
    sensitive = true
    nullable = false
}

variable "tenant_id" {
    type = string
    sensitive = true
    nullable = false
}

variable "subscription_id" {
    type = string
    sensitive = true
    nullable = false
}

provider "azurerm" {
  subscription_id = var.subscription_id
  client_id       = var.principal_id
  client_secret   = var.principal_secret
  tenant_id       = var.tenant_id

  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "snakes"
  location = "northeurope"
}

resource "azurerm_log_analytics_workspace" "logs" {
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  name                = "snakes-logs"
  sku                 = "PerGB2018"
}

resource "azurerm_log_analytics_solution" "logs" {
  location              = azurerm_resource_group.rg.location
  resource_group_name   = azurerm_resource_group.rg.name
  solution_name         = "ContainerInsights"
  workspace_name        = azurerm_log_analytics_workspace.logs.name
  workspace_resource_id = azurerm_log_analytics_workspace.logs.id

  plan {
    product   = "OMSGallery/ContainerInsights"
    publisher = "Microsoft"
  }
}

resource "azurerm_kubernetes_cluster" "cluster" {
  name                  = "snakes-cluster"
  location              = azurerm_resource_group.rg.location
  resource_group_name   = azurerm_resource_group.rg.name
  dns_prefix            = "snakes"
  
  default_node_pool {
    name       = "default"
    node_count = 1
    vm_size    = "Standard_A2_v2"
  }

  linux_profile {
    admin_username = "azureuser"
    ssh_key {
        key_data = file(var.ssh_public_key_file)
    }
  }

  network_profile {
      network_plugin = "kubenet"
      load_balancer_sku = "Standard"
  }

  
  identity {
    type = "SystemAssigned"
  } 
#   service_principal  {
#     client_id = var.serviceprinciple_id
#     client_secret = var.serviceprinciple_key
#   }
}

output "kube_config" {
    value = azurerm_kubernetes_cluster.cluster.kube_config_raw
}

output "cluster_ca_certificate" {
    value = azurerm_kubernetes_cluster.cluster.kube_config.0.cluster_ca_certificate
}

output "client_certificate" {
    value = azurerm_kubernetes_cluster.cluster.kube_config.0.client_certificate
}

output "client_key" {
    value = azurerm_kubernetes_cluster.cluster.kube_config.0.client_key
}

output "host" {
    value = azurerm_kubernetes_cluster.cluster.kube_config.0.host
}