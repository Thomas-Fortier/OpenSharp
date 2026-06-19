Feature: Create conflict surfaces a validation error
  As a developer using OpenSharp
  I want a typed error when I create a resource that already exists
  So that I can distinguish conflicts from other failures (SC-007)

  Scenario: Creating a route that already exists yields a validation error
    Given creating route "dup-route" in namespace "test-ns" returns a conflict
    When I attempt to create a route "dup-route" with host "dup.example.com"
    Then an OpenShiftValidationException is raised
