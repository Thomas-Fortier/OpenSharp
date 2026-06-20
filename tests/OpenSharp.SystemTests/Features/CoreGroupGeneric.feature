Feature: Generic reach into the core API group
  As a developer using OpenSharp
  I want the generic mechanism to reach core (legacy group) resources
  So that I can access any resource type, not only named API groups (FR-005)

  Scenario: Get a core-group resource generically
    Given namespace "default" serves a core "endpoints" named "kubernetes"
    When I get core resource "endpoints" named "kubernetes" in namespace "default"
    Then the core resource name is "kubernetes"
