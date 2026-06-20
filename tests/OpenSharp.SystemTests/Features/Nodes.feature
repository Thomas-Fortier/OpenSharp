Feature: Node access and administration
  As an operator using OpenSharp
  I want to inspect cluster nodes and cordon/uncordon them
  So that I can take nodes out of and back into scheduling rotation (FR-004, FR-006)

  Scenario: List and get nodes with schedulability
    Given the cluster has nodes "node-a,node-b"
    When I list nodes
    Then the node list contains 2 items
    When I get node "node-a"
    Then node "node-a" is schedulable

  Scenario: Cordon then uncordon a node
    Given node "node-a" accepts cordon and uncordon
    When I cordon node "node-a"
    Then the node patch request count is 1
    When I uncordon node "node-a"
    Then the node patch request count is 2

  Scenario: Getting a missing node yields a not-found error
    Given node "ghost" does not exist
    When I attempt to get node "ghost"
    Then a node not-found error is raised

  Scenario: Watch nodes for changes
    Given a node watch emits events "ADDED:node-a,MODIFIED:node-a"
    When I watch nodes with auto-resume disabled
    Then I receive 2 node watch events
