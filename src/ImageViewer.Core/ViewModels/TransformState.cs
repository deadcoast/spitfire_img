using System.Collections.ObjectModel;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.ViewModels;

public sealed class TransformState : ObservableObject
{
    private readonly Stack<TransformOperation> _redoStack = new();

    public ObservableCollection<TransformOperation> PendingTransforms { get; } = [];

    public bool HasUnsavedChanges => PendingTransforms.Count > 0;

    public bool CanUndo => PendingTransforms.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void ApplyTransform(TransformOperation operation)
    {
        PendingTransforms.Add(operation);
        _redoStack.Clear();
        NotifyState();
    }

    public void Undo()
    {
        if (PendingTransforms.Count == 0)
        {
            return;
        }

        var operation = PendingTransforms[^1];
        PendingTransforms.RemoveAt(PendingTransforms.Count - 1);
        _redoStack.Push(operation);
        NotifyState();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        PendingTransforms.Add(_redoStack.Pop());
        NotifyState();
    }

    public void DiscardTransforms()
    {
        PendingTransforms.Clear();
        _redoStack.Clear();
        NotifyState();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }
}
