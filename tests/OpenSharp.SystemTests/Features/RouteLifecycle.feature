Feature: Route lifecycle
  As a developer using OpenSharp
  I want to create, read, update, and delete OpenShift Routes
  So that I can manage external access to my services (SC-004)

  Scenario: Create, read, update, and delete a route
    Given a route "my-route" can be created, read, updated, and deleted in namespace "test-ns"
    When I create a route "my-route" with host "my-route.example.com"
    Then the resulting route host is "my-route.example.com"
    When I get the route "my-route"
    Then the resulting route host is "my-route.example.com"
    When I replace the route "my-route" with host "updated.example.com"
    Then the resulting route host is "updated.example.com"
    When I delete the route "my-route"
    Then the route operation completes without error
