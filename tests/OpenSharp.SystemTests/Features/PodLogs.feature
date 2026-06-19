Feature: Pod logs
  As a developer using OpenSharp
  I want to read and follow container logs
  So that I can inspect what my workloads are doing (FR-005)

  Scenario: Read a snapshot of pod logs
    Given pod "web-1" in namespace "test-ns" has log lines "line1,line2,line3"
    When I read logs for pod "web-1"
    Then the logs contain "line1"
    And the logs contain "line3"

  Scenario: Follow pod logs line by line
    Given pod "web-1" in namespace "test-ns" has log lines "alpha,bravo"
    When I follow logs for pod "web-1"
    Then I receive the log lines "alpha,bravo"
