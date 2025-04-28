using System;

public enum BehaviorStatus
{
  Success,
  Failure,
  Running
}

public interface BehaviorTree
{
  public BehaviorStatus Tick(float dt);
}

public class Success : BehaviorTree
{
  public BehaviorStatus Tick(float dt)
  {
    return BehaviorStatus.Success;
  }
}

public class Failure : BehaviorTree
{
  public BehaviorStatus Tick(float dt)
  {
    return BehaviorStatus.Failure;
  }
}

public class Sequence : BehaviorTree
{
  private BehaviorTree[] children;
  private int currentChildIndex;
  private bool loop;

  public Sequence(BehaviorTree[] children, bool loop = true)
  {
    this.children = children;
    this.loop = loop;
  }

  public BehaviorStatus Tick(float dt)
  {
    if (currentChildIndex < children.Length)
    {
      BehaviorTree currentChild = children[currentChildIndex];
      var status = currentChild.Tick(dt);
      switch (status)
      {
        case BehaviorStatus.Success:
          currentChildIndex++;
          break;
        case BehaviorStatus.Failure:
          currentChildIndex = 0; // Reset to the first child
          return BehaviorStatus.Failure;
        case BehaviorStatus.Running:
          return BehaviorStatus.Running; // Current child is still running
      }
    }

    if (currentChildIndex >= children.Length)
    {
      if (this.loop)
      {
        currentChildIndex = 0; // Reset to the first child if looping
        return BehaviorStatus.Running;
      }
      else
      {
        return BehaviorStatus.Success; // All children have succeeded
      }
    }
    else
    {
      return BehaviorStatus.Running; // Still running
    }
  }
}

public class Try : BehaviorTree
{
  public BehaviorTree[] Children;
  private int currentChildIndex = 0;

  public BehaviorStatus Tick(float dt)
  {
    if (currentChildIndex < Children.Length)
    {
      var status = Children[currentChildIndex].Tick(dt);
      if (status == BehaviorStatus.Success || status == BehaviorStatus.Running)
      {
        return status; // Keep with the current child
      }
      else if (status == BehaviorStatus.Failure)
      {
        currentChildIndex++; // Move to the next child
        return Tick(dt); // Retry with the next child
      }
    }

    return BehaviorStatus.Failure; // All children have failed
  }
}

public class Select : BehaviorTree
{
  public class Choice
  {
    public Func<bool> Condition;
    public BehaviorTree BT;

    public Choice(Func<bool> condition, BehaviorTree child)
    {
      Condition = condition;
      BT = child;
    }
  }

  public Choice[] Children;

  public BehaviorStatus Tick(float dt)
  {
    foreach (var child in Children)
    {
      if (child.Condition())
      {
        return child.BT.Tick(dt);
      }
    }
    return BehaviorStatus.Failure; // No conditions met
  }
}

public class Random : BehaviorTree
{
  public class Choice
  {
    public float Weight;
    public BehaviorTree BT;

    public Choice(float weight, BehaviorTree child)
    {
      Weight = weight;
      BT = child;
    }
  }

  public Choice[] Children;

  private System.Random random = new System.Random();
  private float totalWeight = 0f;
  private bool isInit = false;


  public BehaviorStatus Tick(float dt)
  {
    if (!isInit)
    {
      if (Children == null || Children.Length == 0)
      {
        throw new InvalidOperationException("Children cannot be null or empty.");
      }

      foreach (var child in Children)
      {
        totalWeight += child.Weight;
      }
      isInit = true;
    }

    var select = random.NextDouble() * totalWeight;
    float cumulativeWeight = 0f;
    for (int i = 0; i < Children.Length; i++)
    {
      cumulativeWeight += Children[i].Weight;
      if (select <= cumulativeWeight)
      {
        return Children[i].BT.Tick(dt);
      }
    }
    return Children[^1].BT.Tick(dt); // Fallback to the last child if none selected
  }
}