Feature: Generic force delete
  As a developer using OpenSharp
  I want to delete a resource immediately with a zero grace period
  So that I can remove stuck resources (e.g. force-delete a VMI) (FR-003)

  Scenario: Force-delete a custom resource
    Given widget "w1" in namespace "flightline" can be deleted
    When I force-delete widget "w1" in namespace "flightline"
    Then the widget delete request was sent

  Scenario: Force-deleting a missing resource yields a not-found error
    Given deleting widget "ghost" in namespace "flightline" returns not found
    When I attempt to force-delete widget "ghost" in namespace "flightline"
    Then a generic not-found error is raised
