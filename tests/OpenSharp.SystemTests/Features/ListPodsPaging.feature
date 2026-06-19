Feature: List pods with paging
  As a developer using OpenSharp
  I want to enumerate all pods across multiple pages
  So that I can process large pod lists without needing explicit pagination

  Scenario: Enumerate pods across two pages
    Given namespace "test-ns" has pods "pod-a,pod-b" on page 1 and "pod-c" on page 2
    When I enumerate all pods in namespace "test-ns"
    Then I receive 3 pods total
    And the pod names include "pod-a"
    And the pod names include "pod-c"

  Scenario: List a single page of pods
    Given namespace "test-ns" has pods "web-1,web-2" on a single page
    When I list pods in namespace "test-ns"
    Then the result contains 2 pods
