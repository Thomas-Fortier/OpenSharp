Feature: Scale and rollout workloads
  As a developer using OpenSharp
  I want to scale deployments and trigger rolling restarts
  So that I can operate my workloads programmatically (FR-007)

  Scenario: Scale a deployment to a new replica count
    Given deployment "api" in namespace "test-ns" accepts scale and rollout operations
    When I scale deployment "api" to 3 replicas
    Then the workload operation completes without error

  Scenario: Trigger a rolling restart of a deployment
    Given deployment "api" in namespace "test-ns" accepts scale and rollout operations
    When I trigger a rollout restart of deployment "api"
    Then the workload operation completes without error

  Scenario: Scaling to a negative replica count is rejected
    Given deployment "api" in namespace "test-ns" accepts scale and rollout operations
    When I attempt to scale deployment "api" to -1 replicas
    Then a workload validation error is raised
