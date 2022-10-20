terraform {
    required_providers {
        azurerm = {
            source = "hashicorp/azurerm"
            version = "=2.5.0"
        }
    }
}

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

module "cluster" {
    source              = "./modules/cluster/"
    principal_id        = var.principal_id
    principal_secret    = var.principal_secret
    tenant_id           = var.tenant_id
    subscription_id     = var.subscription_id
    ssh_public_key_file = var.ssh_public_key_file
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
