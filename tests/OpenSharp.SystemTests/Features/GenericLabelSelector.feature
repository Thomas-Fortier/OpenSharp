Feature: Generic resource label-selector filtering
  As a developer using OpenSharp
  I want to filter generically-addressed resources by label
  So that I can select custom resources (e.g. VMs by aircraftType) without first-class support (FR-001)

  Scenario: Filter a custom type by label within a namespace
    Given namespace "flightline" serves widgets "w1,w2,w3" where "w1,w3" carry label "aircraftType=F18"
    When I list widgets in namespace "flightline" filtered by "aircraftType=F18"
    Then the widget list contains 2 items

  Scenario: Filter a custom type by label across all namespaces
    Given all namespaces serve widgets "a,b,c" where "a" carry label "aircraftType=F18"
    When I list widgets in all namespaces filtered by "aircraftType=F18"
    Then the widget list contains 1 items

  Scenario: A selector that matches nothing returns an empty list
    Given namespace "flightline" serves widgets "w1,w2" where "" carry label "aircraftType=F35"
    When I list widgets in namespace "flightline" filtered by "aircraftType=F35"
    Then the widget list contains 0 items
