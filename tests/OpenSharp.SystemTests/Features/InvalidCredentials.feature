Feature: Invalid credentials surface a typed error
  As a developer using OpenSharp
  I want to receive a meaningful typed exception on auth failure
  So that I can handle credential problems programmatically

  Scenario: Listing projects with bad credentials throws OpenShiftAuthenticationException
    Given the cluster returns 401 Unauthorized for project requests
    When I attempt to list projects
    Then an OpenShiftAuthenticationException is thrown

  Scenario: Getting a pod with bad credentials throws OpenShiftAuthenticationException
    Given the cluster returns 401 Unauthorized for pod requests in namespace "test-ns"
    When I attempt to list pods in namespace "test-ns"
    Then an OpenShiftAuthenticationException is thrown
