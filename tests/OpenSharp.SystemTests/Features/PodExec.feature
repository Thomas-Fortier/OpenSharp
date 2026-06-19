@live
Feature: Pod exec
  As a developer using OpenSharp
  I want to execute a command inside a running container and capture its output
  So that I can run operational commands programmatically (FR-006)

  # The Kubernetes exec protocol streams over an upgraded WebSocket connection, which the
  # WireMock simulator cannot reproduce deterministically. This scenario therefore runs only
  # against a real cluster (the opt-in @live category, gated by the OPENSHARP_LIVE env var).
  Scenario: Execute a command and capture stdout and exit code
    Given a live cluster has a running pod "web-1" in namespace "test-ns"
    When I exec command "echo,hello" in pod "web-1"
    Then the exec stdout contains "hello"
    And the exec exit code is 0
