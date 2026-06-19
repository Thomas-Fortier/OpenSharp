Feature: Full CRUD coverage across core and OpenShift resources
  As a developer using OpenSharp
  I want create, read, list, replace, patch, and delete to work for every supported resource
  So that I can manage the full lifecycle of any first-class type (SC-004, SC-005)

  Scenario: ConfigMap full CRUD
    Given the namespace "test-ns" supports full CRUD for configmap "cm1"
    When I exercise full CRUD on "configmaps" for "cm1"
    Then every CRUD operation succeeds
    And the read-back resource is named "cm1"

  Scenario: Secret full CRUD
    Given the namespace "test-ns" supports full CRUD for secret "sec1"
    When I exercise full CRUD on "secrets" for "sec1"
    Then every CRUD operation succeeds
    And the read-back resource is named "sec1"

  Scenario: Service full CRUD
    Given the namespace "test-ns" supports full CRUD for service "svc1"
    When I exercise full CRUD on "services" for "svc1"
    Then every CRUD operation succeeds
    And the read-back resource is named "svc1"

  Scenario: Deployment full CRUD
    Given the namespace "test-ns" supports full CRUD for deployment "dep1"
    When I exercise full CRUD on "deployments" for "dep1"
    Then every CRUD operation succeeds
    And the read-back resource is named "dep1"

  Scenario: DeploymentConfig full CRUD
    Given the namespace "test-ns" supports full CRUD for deploymentconfig "dc1"
    When I exercise full CRUD on "deploymentconfigs" for "dc1"
    Then every CRUD operation succeeds
    And the read-back resource is named "dc1"

  Scenario: Pod full CRUD
    Given the namespace "test-ns" supports full CRUD for pod "pod1"
    When I exercise full CRUD on "pods" for "pod1"
    Then every CRUD operation succeeds
    And the read-back resource is named "pod1"

  Scenario: Project full CRUD
    Given the cluster supports full CRUD for project "proj1"
    When I exercise full CRUD on "projects" for "proj1"
    Then every CRUD operation succeeds
    And the read-back resource is named "proj1"

  Scenario: Route read, list, and patch
    Given the namespace "test-ns" supports full CRUD for route "rt1"
    When I exercise read, list, and patch on routes for "rt1"
    Then every CRUD operation succeeds
    And the read-back resource is named "rt1"

  Scenario: Watch a namespaced custom resource resumes and ends cleanly
    Given a deploymentconfig watch in namespace "test-ns" emits an added event for "dc-watch"
    When I watch deploymentconfigs in namespace "test-ns" with auto-resume disabled
    Then I receive 1 workload watch event

  Scenario: Watch a cluster-scoped resource
    Given a project watch emits an added event for "proj-watch"
    When I watch projects with auto-resume disabled
    Then I receive 1 workload watch event

  Scenario: Generic create and delete and cluster-scoped list
    Given the cluster serves generic widgets for create, delete, and cluster listing
    When I create and delete a generic widget "gw1" and list cluster widgets
    Then every CRUD operation succeeds

  Scenario: Generic namespaced get and list and cluster get
    Given the cluster serves generic widgets for namespaced get, namespaced list, and cluster get
    When I get and list namespaced generic widgets and get a cluster widget
    Then every CRUD operation succeeds

  Scenario Outline: Watch a namespaced resource and end cleanly
    Given a "<plural>" watch in namespace "test-ns" emits an added event for "w-<plural>"
    When I watch "<plural>" in namespace "test-ns" with auto-resume disabled
    Then I receive 1 workload watch event

    Examples:
      | plural      |
      | configmaps  |
      | secrets     |
      | services    |
      | deployments |
      | routes      |
