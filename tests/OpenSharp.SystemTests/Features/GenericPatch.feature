Feature: Generic resource patch
  As a developer using OpenSharp
  I want to apply a partial update to a generically-addressed resource
  So that I can modify custom resources without first-class support (FR-002)

  Scenario: Patch a custom resource and observe the change
    Given widget "w1" in namespace "flightline" accepts a patch setting label team "qa"
    When I patch widget "w1" in namespace "flightline" setting label team "qa"
    Then the patched widget has label team "qa"

  Scenario: An invalid patch surfaces a validation error
    Given patching widget "w1" in namespace "flightline" is rejected as invalid
    When I attempt to patch widget "w1" in namespace "flightline"
    Then a generic validation error is raised
