namespace Infrastructure {

    public class SimpleEffect : AbstractEffect {

        public override void Show() {
            gameObject.SetActive(true);
        }

        public override void Hide() {
            gameObject.SetActive(false);
        }
    }
}