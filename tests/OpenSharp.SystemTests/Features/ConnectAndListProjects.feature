Feature: Connect and list projects
  As a developer using OpenSharp
  I want to list OpenShift projects
  So that I can see all namespaces available to me

  Scenario: List all projects returns non-empty result
    Given the cluster has projects "alpha,beta,gamma"
    When I list all projects
    Then the result contains 3 projects
    And the project names include "alpha"

  Scenario: List projects on an empty cluster returns an empty list
    Given the cluster has no projects
    When I list all projects
    Then the result contains 0 projects

  Scenario: Get a single project by name
    Given the cluster has projects "my-project"
    When I get the project named "my-project"
    Then the project name is "my-project"
