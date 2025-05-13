namespace Kylin.LWDI
{
    public class BaseModel : IDependencyObject, IInjectable
    {
        protected BaseModel()
        {
            Inject();
        }
        
        public void Inject(IScope scope = null)
        {
            DependencyInjector.Inject(this, scope);
        }
    }
}