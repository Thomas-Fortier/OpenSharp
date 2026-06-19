Feature: Watch pods for changes
  As a developer using OpenSharp
  I want to receive added, modified, and deleted events for pods
  So that I can react to cluster state changes (FR-008)

  Scenario: Receive add, modify, and delete events
    Given a pod watch in namespace "test-ns" emits events "ADDED:p1,MODIFIED:p1,DELETED:p1"
    When I watch pods in namespace "test-ns" with auto-resume disabled
    Then I receive 3 watch events
    And watch event 1 is of type "Added"
    And watch event 2 is of type "Modified"
    And watch event 3 is of type "Deleted"
