Feature: Generic resource escape hatch
  As a developer using OpenSharp
  I want to get and list resource types that are not first-class in the library
  So that I can reach any API by group, version, and plural (FR-009)

  Scenario: List and get a generic resource by group, version, and plural
    Given the cluster has "example.com/v1/widgets" named "w1,w2" in namespace "test-ns"
    When I list generic resources "example.com/v1/widgets" in namespace "test-ns"
    Then the generic list contains 2 items
    When I get generic resource "w1" of "example.com/v1/widgets" in namespace "test-ns"
    Then the generic resource name is "w1"
