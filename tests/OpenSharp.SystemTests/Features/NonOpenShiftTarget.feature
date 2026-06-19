Feature: OpenShift-specific resource on a non-OpenShift target
  As a developer using OpenSharp
  I want a clear, typed error when I use a Route against a plain Kubernetes cluster
  So that I immediately understand the target does not support OpenShift resources (FR-015)

  Scenario: Creating a route on a plain Kubernetes cluster fails clearly
    Given the cluster does not serve the OpenShift Route API in namespace "test-ns"
    When I attempt to create a route "any-route" with host "any.example.com"
    Then an OpenShiftValidationException is raised
    And the route error message mentions "OpenShift extension"
