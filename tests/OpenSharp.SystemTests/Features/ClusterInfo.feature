Feature: Cluster information and capability discovery
  As a developer using OpenSharp
  I want to retrieve cluster info and check whether a resource type is served
  So that I can target the right cluster and degrade gracefully (FR-007, FR-008)

  Scenario: Retrieve cluster information
    Given the cluster reports version "v1.28.3"
    When I get cluster info
    Then the cluster server version is "v1.28.3"
    And the cluster endpoint is set
    And the cluster is reachable

  Scenario: An unreachable cluster surfaces a connection error
    Given the cluster version endpoint is unavailable
    When I attempt to get cluster info
    Then a cluster connection error is raised

  Scenario: A served resource type is available
    Given the cluster serves resources "routes,routestatus" in group "route.openshift.io" version "v1"
    When I check availability of "routes" in group "route.openshift.io" version "v1"
    Then the resource type is available

  Scenario: An unavailable resource type is reported as not available
    Given the cluster does not serve group "route.openshift.io" version "v1"
    When I check availability of "routes" in group "route.openshift.io" version "v1"
    Then the resource type is not available
