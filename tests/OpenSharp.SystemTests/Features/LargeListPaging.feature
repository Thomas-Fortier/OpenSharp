Feature: Bounded-memory enumeration over large result sets
  As a developer using OpenSharp
  I want to enumerate very large collections by transparently following continuation tokens
  So that memory stays bounded even with thousands of resources (SC-006)

  Scenario: Enumerate 10000 pods across 100 pages
    Given namespace "test-ns" has 10000 pods across pages of 100
    When I enumerate all pods in namespace "test-ns"
    Then I receive 10000 pods total
