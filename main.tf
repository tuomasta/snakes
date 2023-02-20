terraform {
    required_providers {
        azurerm = {
            source = "hashicorp/azurerm"
            version = "=3.44.1"
        }
    }
    backend "azurerm" {
        container_name       = "terraform-state"
        key                  = "snakes.tfstate"
        
        # use inline configurations with -backend-config=
        # resource_group_name  = "tuomas-general"
        # storage_account_name = "tuomascommonstorage"       
    }
}

variable "ssh_public_key_file" {
    type = string
    default = "./snakes.pub"
}


provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "snakes"
  location = "northeurope"
}

module "cluster" {
    source              = "./modules/cluster/"
    ssh_public_key_file = var.ssh_public_key_file
    rg_name = azurerm_resource_group.rg.name

    depends_on = [azurerm_resource_group.rg]
}

module "k8s" {
    source                = "./modules/k8s/"
    host                  = "${module.cluster.host}"
    client_certificate    = "${base64decode(module.cluster.client_certificate)}"
    client_key            = "${base64decode(module.cluster.client_key)}"
    cluster_ca_certificate= "${base64decode(module.cluster.cluster_ca_certificate)}"
}

output "kube_config" {
    value = module.cluster.kube_config
    sensitive = true
}
